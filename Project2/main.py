import pygame

pygame.init()
logo_icon = pygame.image.load("Tic_tac_toe.svg")
pygame.display.set_caption("Tres en ratlla")
pygame.display.set_icon(logo_icon)

resolution = (500, 500)
screen = pygame.display.set_mode(resolution)

size = 3
stride = 3
map = []

actual_player = 1
game_over = False
end_time = 0
deja_clique = False

for x in range(size):
    map.append([])
    for y in range(size):
        map[x].append(0)

def render():
    for x in range(size - 1):
        posX = resolution[0] / size * (x + 1)
        pygame.draw.rect(screen, (2, 171, 144), (posX - 3, 0, 6, resolution[1]))

    for y in range(size - 1):
        posY = resolution[1] / size * (y + 1)
        pygame.draw.rect(screen, (2, 171, 144), (0, posY - 3, resolution[0], 6))

    for x in range(size):
        for y in range(size):
            posX = (resolution[0] / size) * (x + 0.5)
            posY = (resolution[1] / size) * (y + 0.5)

            if map[x][y] == 1:
                pygame.draw.line(screen, (80, 90, 150), (posX - 35, posY - 35), (posX + 35, posY + 35), 30)
                pygame.draw.line(screen, (80, 90, 150), (posX - 35, posY + 35), (posX + 35, posY - 35), 30)
            elif map[x][y] == 2:
                pygame.draw.circle(screen, (150, 80, 90), (posX, posY), 50)

def play():
    global actual_player, map, deja_clique, game_over
    
    if game_over:
        return

    mouse = pygame.mouse.get_pressed()
    if mouse[0]:
        if not deja_clique:
            mouse_ps = pygame.mouse.get_pos()
            cellX = int(mouse_ps[0] * size / resolution[0])
            cellY = int(mouse_ps[1] * size / resolution[1])
            
            if 0 <= cellX < size and 0 <= cellY < size:
                cell_value = map[cellX][cellY]
                if cell_value == 0:
                    map[cellX][cellY] = actual_player
                    if actual_player == 1:
                        actual_player = 2
                    elif actual_player == 2:
                        actual_player = 1
            deja_clique = True
    else:
        deja_clique = False

def check():
    global map, game_over, end_time
    
    if game_over:
        return

    check_X = size - stride + 1

    for y in range(size):
        for x in range(check_X):
            if map[x][y] == 1:
                count = 0
                for i in range(stride):
                    if map[x + i][y] == 1:
                        count += 1
                    else:
                        break
                if count == stride:
                    print("Player 1 wins")
                    game_over = True
                    end_time = pygame.time.get_ticks()
                    return

    for q in range(size):
        for e in range(check_X):
            if map[e][q] == 2:
                count = 0
                for t in range(stride):
                    if map[e + t][q] == 2:
                        count += 1
                    else:
                        break
                if count == stride:
                    print("Player 2 wins")
                    game_over = True
                    end_time = pygame.time.get_ticks()
                    return
        
def reset_game():
    global map, actual_player, game_over
    map = []
    for x in range(size):
        map.append([])
        for y in range(size):
            map[x].append(0)
    actual_player = 1
    game_over = False

clock = pygame.time.Clock()
running = True

while running:
    for evento in pygame.event.get():
        if evento.type == pygame.QUIT:
            running = False

    screen.fill((2, 194, 164))
    render()
    play()
    check()
    
    if game_over:
        if pygame.time.get_ticks() - end_time >= 2000:
            reset_game()

    pygame.display.flip()
    clock.tick(60)

pygame.quit()